use Std.Ops.Sub;

struct Num { val: i32 }

impl Sub(Num) for Num {
    let Output :! Sized = Num;
    fn Sub(lhs: Num, rhs: Num) -> Num {
        make Num { val: lhs.val - rhs.val }
    }
}

fn main() -> i32 {
    let a = make Num { val: 10 };
    let b = make Num { val: 3 };
    let c = a - b;
    let d = a - b;
    0
}
