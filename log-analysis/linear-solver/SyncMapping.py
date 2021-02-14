
import os

class SyncMapping():
    def __init__(self, mapping_dir):
        self.map_dir = {}
        self.rel_set = set()
        self.acq_set = set()
        log_files = [f for f in os.listdir(mapping_dir) if f.endswith(".mp")]
        for log_file in log_files:
            with open(os.path.join(mapping_dir, log_file)) as fd:
                #print("load mapping: ", log_file)
                for line in fd:
                    items = line.split(' ')
                    rel = items[0]
                    acq = items[2][:-1]
                    if rel not in self.rel_set:
                        self.rel_set.add(rel)
                    if acq not in self.acq_set:
                        self.acq_set.add(acq)
                    if rel not in self.map_dir:
                        self.map_dir[rel] = []
                    self.map_dir[rel].append(acq)
        #for op in self.rel_set:
        #    print("REL:", op)
        #for op in self.acq_set:
        #    print("ACQ:", op)
    def in_rel_set(self, op):
        return op in self.rel_set
    def in_acq_set(self, op):
        return op in self.acq_set
    def mapping_to_list(self, op):
        if op in self.map_dir:
            return self.map_dir[op]
        return set()
