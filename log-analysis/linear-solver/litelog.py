#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from typing import List, Dict
import bisect
import re

import numpy as np
import time

from APISpecification import APISpecification
from LogEntry import LogEntry

class LiteLog:

    @staticmethod
    def load_log(logpath: str) -> 'LiteLog':

        log = LiteLog()
        my_list = []
        with open(logpath) as fd:
            for line in fd:
                try:
                    entry = LogEntry.parse(line)
                    my_list.append(entry)
                except:
                    print('error when loading',line)
                    print('   in ', logpath)
                    raise

        log.log_list_ = np.array(my_list)

        return log

    def __iter__(self):
        return iter(self.log_list_)

    def __getitem__(self, index: int):
        return self.log_list_[index]

    def __len__(self):
        return len(self.log_list_)

    def append(self, log_entry: LogEntry):
        self.log_list_.append(log_entry)

    def range_by(self, start_tsc: int, end_tsc: int, ltsc: bool) -> 'LiteLog':

        '''
        Find log entries whose tsc: start_tsc < tsc < end_tsc
        When left_one_more is True, add one more log whose tsc may be less then start_tsc

        ltsc = true : we compare with the starting time-stamp
        ltsc = false: we compare with the finishing time-stamp
        '''
        left_key = LogEntry.TscCompare(start_tsc, ltsc)
        right_key = LogEntry.TscCompare(end_tsc, ltsc)

        left_index = bisect.bisect_right(self.log_list_, left_key)
        right_index = bisect.bisect_left(self.log_list_, right_key)

        '''
        if left_one_more:
            if left_index > 0:
                left_index -= 1

        if right_one_less:
            if right_index > left_index:
                right_index -= 1
        '''
        log = LiteLog()
        log.log_list_ =  self.log_list_[left_index: right_index]
        return log


if __name__ == "__main__":
    log = LiteLog.load_log('outputs/outputs/1.litelog')
    for x in log:
        print(x)
        print()

    print("===============")

    for x in log.range_by(637207729813661304, 637207729814207440):
        print(x)
        print()
