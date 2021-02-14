#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import argparse
import os

from litelog import LiteLog, LogEntry
from constraint import Variable, ConstaintSystem
from APISpecification import APISpecification
from nearmiss import near_miss_encode, organize_by_obj_id

from collections import defaultdict
from typing import Dict

import time

checkpoint_dir = './sh-temp'
test_sum = 0

def generate_constraints_for_every_test(log_dir, test, constraints, test_sum):

    test_dir = os.path.join(log_dir, test)
    if os.path.isfile(test_dir):
        return
    log_files = [f for f in os.listdir(test_dir) if f.endswith(".litelog")]

    if len(log_files) < 2:
        return test_sum
    print(f'Test {test_dir} log files size : {len(log_files)}')
    test_sum = test_sum + 1

    thread_log: Dict[str, LiteLog] = {
        log_name: LiteLog.load_log(os.path.join(test_dir, log_name))
        for log_name in log_files
    }

    for thread_id, log in thread_log.items():
        for log_entry in log:
            log_entry.thread_id_ = thread_id

    obj_id_log, obj_id_threadlist = organize_by_obj_id(thread_log)

    near_miss_encode(constraints, thread_log, obj_id_log, obj_id_threadlist)

    return test_sum

if __name__ == "__main__":

    dirparser = argparse.ArgumentParser()
    dirparser.add_argument('--batch', help='the log directory')
    dirparser.add_argument('--balance', help='set lambda')
    dirparser.add_argument('-refine',  action='store_true')
    args = dirparser.parse_args()

    constraints = ConstaintSystem()
    lamd = 0.2
    if args.balance:
        lamd = float(args.balance)
    constraints.set_lambda(lamd)
    APISpecification.Initialize()

    if args.refine :
        constraints.load_checkpoint(checkpoint_dir)
        print(args.batch)
        log_dir = args.batch
        for test in os.listdir(log_dir):
            test_sum = generate_constraints_for_every_test(log_dir, test, constraints, test_sum)
        constraints.build_constraints()
        print("Total MT tests size ", test_sum)
        constraints._lp_solve()
        constraints.print_debug_info()
        constraints.print_compare_result(constraints.pre_rel_vars_, constraints.pre_acq_vars_)
        constraints.save_mapping(checkpoint_dir)
        constraints.save_problem(checkpoint_dir)
        constraints.save_info(checkpoint_dir)
    else:
        print(args.batch)
        log_dir = args.batch
        for test in os.listdir(log_dir):
            test_sum = generate_constraints_for_every_test(log_dir, test, constraints, test_sum)
        constraints.build_constraints()
        print("Total MT tests size ", test_sum)
        constraints._lp_solve()
        #constraints.print_debug_info()
        constraints.print_compare_result([], [])
        constraints.save_info(checkpoint_dir)
