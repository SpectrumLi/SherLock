from typing import List, Dict
import bisect
import re

import numpy as np
import time

from APISpecification import APISpecification

class LogEntry():
    map_api_entry: Dict[str, Dict[str,int]]  = {}
    map_api_timegap: Dict[str, List[int]] = {}
    objid_to_int: Dict[str, int] = {}
    int_to_objid: Dict[int, str] = {}
    @staticmethod
    def parse(line: str) -> 'LogEntry':
        #print("load ", line)
        tup = line.strip().split('|')
        return LogEntry(tup)

    @classmethod
    def get_objid(cls, objid: str):
        if objid not in cls.objid_to_int:
            k = len(cls.objid_to_int)
            cls.objid_to_int[objid] = k
            cls.int_to_objid[k] = objid
        return cls.objid_to_int[objid]

    def __init__(self, tup):
        if len(tup) != 6:
            for t in tup:
                print(t)
        assert len(tup) == 6

        #start = time.time()
        self.start_tsc_ = int(tup[0].strip())
        objid = tup[1].strip()
        #self.object_id_ = tup[1].strip()
        self.object_id_ = LogEntry.get_objid(objid)
        self.op_type_ = tup[2].strip()
        self.operand_ = self.shrink_name(tup[3].strip())

        #self.is_write_ = self.op_type_ == "Write" or (self.op_type_ == "Call" and APISpecification.Is_Write_API(self.operand_) )
        #self.is_read_  = self.op_type_ == "Read"  or (self.op_type_ == "Call" and APISpecification.Is_Read_API( self.operand_) )
        self.is_write_ = self.op_type_ == "Write"
        self.is_read_  = self.op_type_ == "Read"
        #self.object_id_ = LogEntry.get_objid("01")
        #self.is_write_ = self.op_type_ == "Call" and (APISpecification.Is_Write_API(self.operand_) or APISpecification.Is_Read_API( self.operand_))
        #self.is_read_  = self.op_type_ == "Call" and APISpecification.Is_Read_API( self.operand_)

        self.is_sleep_ = self.op_type_ == "Sleep"

        self.location_ = tup[4].strip()
        self.time_gap_ = int(tup[5].strip())
        #add to the paper
        if  "-End" in self.operand_ or "-Begin" in self.operand_:
            self.time_gap_ = 1
        self.finish_tsc_ = self.start_tsc_ + self.time_gap_
        self.thread_id_ = -1  # Fixed after log is loaded
        self.in_window_ = False
        self.description_ = self.op_type_ + "|" + self.operand_

        if self.description_ not in LogEntry.map_api_entry:
            LogEntry.map_api_entry[self.description_] = {}
        if self.location_ not in LogEntry.map_api_entry[self.description_]:
            LogEntry.map_api_entry[self.description_][self.location_] =1
        else:
            LogEntry.map_api_entry[self.description_][self.location_] +=1

        if self.description_ not in LogEntry.map_api_timegap:
            LogEntry.map_api_timegap[self.description_] = []
        LogEntry.map_api_timegap[self.description_].append(self.time_gap_)

    def shrink_name(self, s: str):
        if '__' in s:
            return s
        t = s.replace('`1','')
        t = t.replace('`2','')
        t = re.sub('<.*?>','',t)
        return t;
    def __str__(self):
        s = (f'Tsc: {self.start_tsc_}',
             f'ThreadID: {self.thread_id_}',
             f'Object ID: {self.object_id_}',
             f'Op type: {self.op_type_}',
             f'Operand: {self.operand_}',
             f'Location: {self.location_}',
             )
        return '\n'.join(s)

    def is_write(self) -> bool:
        return self.is_write_

    def is_read(self) -> bool:
        return self.is_read_

    def is_candidate(self) -> bool:
        #if self.op_type_.lower() != 'call':
        #    return False

        if '::.ctor' in self.operand_ and 'Call' in self.op_type_ :
            return False

        #if 'k__BackingField' in self.operand_:
        #    return False

        if '::get_' in self.operand_ and 'Call' in self.op_type_:
            return False

        if '::set_' in self.operand_ and 'Call' in self.op_type_:
            return False

        #if self.is_sleep_:
        #    return False

        return True

    def is_conflict(self, another: 'LogEntry') -> bool:
        if ((self.thread_id_ != another.thread_id_) and
            (self.is_write() or another.is_write())):
            return True

        return False

    #
    # A trick to exploit the binary search
    # because bisect does not support customized comparison directly
    #
    class TscCompare:
        def __init__(self, tsc, ltsc):
            self.tsc_ = tsc
            self.ltsc_ = ltsc

        def __lt__(self, other: 'LogEntry'):
            if self.ltsc_:
                return self.tsc_ < other.start_tsc_
            return self.tsc_ < other.finish_tsc_

    def __lt__(self, other: 'TscCompare'):
        if other.ltsc_:
            return self.start_tsc_ < other.tsc_
        return self.finish_tsc_ < other.tsc_
