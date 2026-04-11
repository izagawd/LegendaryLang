use Std.Ops.Add;

struct Num { val: i32 }

impl Add(Num) for Num {
    let Output :! Sized = Num;
    fn Add(lhs: Num, rhs: Num) -> Num {
        make Num { val: lhs.val + rhs.val }
    }
}

fn main() -> i32 {
    let a = make Num { val: 5 };
    let b = make Num { val: 3 };
    let c = a + b;
    let d = a + b;
    0
}
