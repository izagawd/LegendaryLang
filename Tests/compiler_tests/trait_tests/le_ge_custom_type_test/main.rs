use Std.Ops.PartialEq;
use Std.Ops.PartialOrd;

struct Score { val: i32 }
impl Copy for Score {}

impl PartialEq(Score) for Score {
    fn Eq(lhs: Score, rhs: Score) -> bool { lhs.val == rhs.val }
}

impl PartialOrd(Score) for Score {
    fn Lt(lhs: Score, rhs: Score) -> bool { lhs.val < rhs.val }
    fn Gt(lhs: Score, rhs: Score) -> bool { lhs.val > rhs.val }
}

fn main() -> i32 {
    let a = make Score { val: 5 };
    let b = make Score { val: 10 };
    let c = make Score { val: 5 };
    let r1 = if a <= b { 1 } else { 0 };
    let r2 = if a <= c { 10 } else { 0 };
    let r3 = if b >= a { 100 } else { 0 };
    let r4 = if a >= c { 1000 } else { 0 };
    r1 + r2 + r3 + r4
}
