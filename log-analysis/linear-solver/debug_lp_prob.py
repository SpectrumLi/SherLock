#!/usr/bin/env python3
# -*- coding: utf-8 -*-


import flipy

prob = flipy.LpReader.read(open('./problem.lp'))

solver = flipy.CBCSolver()
status = solver.solve(prob)

print(status)