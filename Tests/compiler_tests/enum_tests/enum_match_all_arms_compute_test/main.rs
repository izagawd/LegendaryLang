enum Op {
    Add(i32, i32),
    Mul(i32, i32),
    Neg(i32)
}

fn eval(op: Op) -> i32 {
    match op {
        Op.Add(a, b) => a + b,
        Op.Mul(a, b) => a * b,
        Op.Neg(x) => 0 - x
    }
}

fn main() -> i32 {
    eval(Op.Add(10, 5)) + eval(Op.Mul(3, 4)) + eval(Op.Neg(3))
}
