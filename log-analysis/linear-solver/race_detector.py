#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import argparse
import os

from litelog import LiteLog, LogEntry
from SyncMapping import SyncMapping
from constraint import Variable, ConstaintSystem
from APISpecification import APISpecification
from nearmiss import near_miss_encode, organize_by_obj_id

from collections import defaultdict
from typing import Dict

import time

mapping_dir = 'E:/Sherlock/idelay/log-analyze/maps'
test_sum = 0

inferred_hb = {}

def load_synchronization_mapping():
    dict = {}
    log_files = [f for f in os.listdir(mapping_dir) if f.endswith(".mp")]
    for log_file in log_files:
        with open(os.path.join(mapping_dir, log_file)) as fd:
            #print("load mapping: ", log_file)
            for line in fd:
                items = line.split(' ')
                rel = items[0]
                acq = items[2]
                if rel not in dict:
                    dict[rel] = []
                dict[rel].append(acq)
    return dict

def check_thread_unsafe_protection(thread_log, obj_id_log, obj_id_threadlist, sync_dict, protection):

    near_miss_dict = {}

    for objid in obj_id_log:
        log = obj_id_log[objid]
        #remove too less opreations on the object
        if len(log) < 2 or LogEntry.int_to_objid[objid] == 'null':
            continue
        #remove the operations only sequential accesses
        ex_entry = log[0]
        if len(obj_id_threadlist[ex_entry.object_id_]) < 2:
            continue
        #remove the operations on null
        objidstr = LogEntry.int_to_objid[objid]
        if len(objidstr) < 42 and '0000-0000-' in objidstr:
            continue

        for i in range(len(log)):
            end_log_entry = log[i]
            for j in range(i - 1, -1, -1):
                start_log_entry = log[j]
                start_tsc, end_tsc = start_log_entry.finish_tsc_, end_log_entry.start_tsc_

                if not close_enough(start_tsc, end_tsc):
                    break

                if not start_log_entry.is_conflict(end_log_entry):
                    continue

                sig = find_signature(start_log_entry, end_log_entry)
                if sig in inferred_hb:
                    continue
                if sig in near_miss_dict and near_miss_dict[sig] > 10:
                    continue
                if sig not in near_miss_dict:
                    near_miss_dict[sig] = 0
                near_miss_dict[sig] += 1

                if check_race(end_log_entry, start_log_entry, thread_log, sync_dict, protection):
                    #print(end_log_entry.description_,"@", end_log_entry.location_, " || ",start_log_entry.description_,"@", start_log_entry.location_)
                    continue

                inferred_hb[sig] = 1
                print(end_log_entry.description_,"@", end_log_entry.location_, " -> ",start_log_entry.description_,"@", start_log_entry.location_)
                #print(log[i].start_tsc_, " ",log[i].description_)
    return

def find_signature(l1: LogEntry, l2: LogEntry):
    s1 = l1.location_
    s2 = l2.location_
    return s1 + "->" + s2;

def close_enough(x, y):
    DISTANCE = 10000000
    if x > y:
        x, y = y, x
    return y < x + DISTANCE

def find_first_race(thread_log, obj_id_log, obj_id_threadlist, sync_dict, protection):

    progress = 0
    race_A = None
    race_B = None
    race_tsc = -1

    for objid in obj_id_log:

        progress = progress + 1
        log = obj_id_log[objid]

        #remove too less opreations on the object
        if len(log) < 2 or LogEntry.int_to_objid[objid] == 'null':
            continue

        #remove the operations only sequential accesses
        ex_entry = log[0]
        if len(obj_id_threadlist[ex_entry.object_id_]) < 2:
            continue

        #remove the operations on null
        objidstr = LogEntry.int_to_objid[objid]
        if len(objidstr) < 42 and '0000-0000-' in objidstr:
            continue

        last_read_entry = None
        last_write_entry = None

        for i in range(len(log)):
            log_entry = log[i]
            if check_race(log_entry, last_read_entry, thread_log, sync_dict, protection):
                if race_tsc < 0 or race_tsc > log_entry.start_tsc_:
                    race_A = last_read_entry
                    race_B = log_entry
                    race_tsc = log_entry.start_tsc_
                    break

            if check_race(log_entry, last_write_entry, thread_log, sync_dict, protection):
                if race_tsc < 0 or race_tsc > log_entry.start_tsc_:
                    race_A = last_write_entry
                    race_B = log_entry
                    race_tsc = log_entry.start_tsc_
                    break

            if (log_entry.is_read_):
                last_read_entry  = log_entry
            else:
                last_write_entry = log_entry
    if race_A is not None and race_B is not None:
        print("find the race!!! "+ str(race_B.start_tsc_))
        print("    " + race_A.description_ + " @" + race_A.location_)
        print("    " + race_B.description_ + " @" + race_B.location_)
        print()

def check_race(cur_entry, pre_entry, thread_log, sync_dict, protection):
    if pre_entry is None:
        return False
    if cur_entry.is_read_ and pre_entry.is_read_:
        return False
    if cur_entry.thread_id_ == pre_entry.thread_id_:
        return False

    start_tsc, end_tsc = pre_entry.finish_tsc_, cur_entry.start_tsc_

    rel_log_list = [
        log_entry.description_
        for log_entry in thread_log[pre_entry.thread_id_].
        range_by(start_tsc, end_tsc, ltsc = True)
        if sync_dict.in_rel_set(log_entry.description_)
    ]
    if sync_dict.in_rel_set(pre_entry.description_):
        rel_log_list.append(pre_entry.description_)

    acq_log_list = [
        log_entry.description_
        for log_entry in thread_log[cur_entry.thread_id_].
        range_by(start_tsc, end_tsc, ltsc = False)
        if sync_dict.in_acq_set(log_entry.description_)
    ]
    if sync_dict.in_acq_set(cur_entry.description_):
        acq_log_list.append(cur_entry.description_)

    for des in rel_log_list:
        for des2 in sync_dict.mapping_to_list(des):
            if des2 in acq_log_list:
                protection.add(f'protection: {des} -> {des2}')
                return False

    '''
    acq_orignal_log_list = [
            log_entry.description_
            for log_entry in thread_log[cur_entry.thread_id_].
            range_by(start_tsc, end_tsc, ltsc = False)
            if log_entry.is_candidate()
        ]


    print("Find a nearmiss : ")
    print("Releasing window : ")
    for log_entry in rel_log_list:
        print(log_entry)
    print("Acquiring window : ")
    for log_entry in acq_orignal_log_list:
        print(log_entry)
    '''
    return True

def find_first_race_for_every_test(log_dir, test, sync_dict, protection):

    test_dir = os.path.join(log_dir, test)

    log_files = [f for f in os.listdir(test_dir) if f.endswith(".litelog")]

    if len(log_files) < 2:
        return
    print(f'Test {test_dir} log files size : {len(log_files)}')

    thread_log: Dict[str, LiteLog] = {
        log_name: LiteLog.load_log(os.path.join(test_dir, log_name))
        for log_name in log_files
    }

    for thread_id, log in thread_log.items():
        for log_entry in log:
            log_entry.thread_id_ = thread_id

    obj_id_log, obj_id_threadlist = organize_by_obj_id(thread_log)

    #check the first race
    #find_first_race(thread_log, obj_id_log, obj_id_threadlist, sync_dict, protection)

    #check the protection for thread-unsafe apis
    check_thread_unsafe_protection(thread_log, obj_id_log, obj_id_threadlist, sync_dict, protection)

    return

if __name__ == "__main__":

    dirparser = argparse.ArgumentParser()
    dirparser.add_argument('--batch', help='the log directory')
    args = dirparser.parse_args()
    APISpecification.Initialize()

    print(args.batch)
    log_dir = args.batch
    protection = set()
    sync_dict = SyncMapping(mapping_dir)
    for test in os.listdir(log_dir):
        find_first_race_for_every_test(log_dir, test, sync_dict, protection)

    #print("Total protection")
    #for p in protection:
    #    print(p)
