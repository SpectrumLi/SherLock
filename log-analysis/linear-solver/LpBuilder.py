from typing import List, Dict, Set, Tuple
# from pulp import LpVariable, lpSum, LpMinimize
# from pulp import LpProblem, LpConstraint, LpConstraintGE, LpStatus
from flipy import LpVariable
import flipy

class LpBuilder:
    cons_counter = 0

    @classmethod
    def var(cls, name: str, up_bound: int):
        return flipy.LpVariable(name, var_type=flipy.VarType.Integer,
                                low_bound=0, up_bound=up_bound)

    @classmethod
    def sum_expr_weight(cls, lp_var_list: List[LpVariable], increase_flag: bool):
        delta = 0.01
        min_weight = 0.5
        n = len(lp_var_list)

        if not increase_flag:
            return flipy.LpExpression(expression = {v : max(1 - idx*delta, min_weight) for idx,v in enumerate(lp_var_list)})

        return flipy.LpExpression(expression = {v : max(1 - (n - idx - 1)*delta, min_weight) for idx,v in enumerate(lp_var_list)})

    @classmethod
    def sum_expr_weight_1(cls, lp_var_list: List[LpVariable]):
        return flipy.LpExpression(expression={v: 1 for v in lp_var_list})

    @classmethod
    def both_roles_constraint(cls, v1, v2, v3):
        lhs = flipy.LpExpression(expression={v1: 1, v2: 1})
        rhs = flipy.LpExpression(expression={v3: 1}, constant=1)
        return flipy.LpConstraint(lhs, 'leq', rhs, name=f'_C_BothRolesPenalty_{cls._cons_id()}')
        
    @classmethod
    def const_expr(cls, value: int):
        return flipy.LpExpression(constant=value)

    @classmethod
    def problem(cls, name: str):
        return flipy.LpProblem(name)

    @classmethod
    def _cons_id(cls):
        cls.cons_counter += 1
        return cls.cons_counter

    @classmethod
    def constraint_sum_geq_weight_increase(cls, lp_var_list: List[LpVariable], value: int, typ ='default'):
        lhs = cls.sum_expr_weight(lp_var_list, True) # true means increase
        rhs = cls.const_expr(value)

        return flipy.LpConstraint(lhs, 'geq', rhs, name=f'_C_{typ}_{cls._cons_id()}')

    @classmethod
    def constraint_sum_geq_weight_decrease(cls, lp_var_list: List[LpVariable], value: int, typ ='default'):
        lhs = cls.sum_expr_weight(lp_var_list, False) # false means decrease
        rhs = cls.const_expr(value)

        return flipy.LpConstraint(lhs, 'leq', rhs, name=f'_C_{typ}_{cls._cons_id()}')

    @classmethod
    def constraint_sum_geq_weight_1(cls, lp_var_list: List[LpVariable], value: int, typ ='default'):
        lhs = cls.sum_expr_weight_1(lp_var_list)
        rhs = cls.const_expr(value)

        return flipy.LpConstraint(lhs, 'geq', rhs, name=f'_C_{typ}_{cls._cons_id()}')

    @classmethod
    def constraint_sum_leq_weight_1(cls, lp_var_list: List[LpVariable], value: int, typ ='default'):
        lhs = cls.sum_expr_weight_1(lp_var_list)
        rhs = cls.const_expr(value)

        return flipy.LpConstraint(lhs, 'leq', rhs, name=f'_C_{typ}_{cls._cons_id()}')

    @classmethod
    def constraint_sum_eq_weight_1(cls, lp_var_list: List[LpVariable], value: int, typ ='default'):
        lhs = cls.sum_expr_weight_1(lp_var_list)
        rhs = cls.const_expr(value)

        return flipy.LpConstraint(lhs, 'eq', rhs, name=f'_C_{typ}_{cls._cons_id()}')

    @classmethod
    def constraint_vars_eq(cls, v1: List[LpVariable], v2: List[LpVariable], typ ='default'):
        lhs = cls.sum_expr_weight_1(v1)
        rhs = cls.sum_expr_weight_1(v2)

        return flipy.LpConstraint(lhs, 'eq', rhs, name=f'_C_{typ}_{cls._cons_id()}')
