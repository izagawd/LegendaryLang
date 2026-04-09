use Std.Ops.PartialEq;
use Std.Ops.PartialOrd;

struct Score { val: i32 }
impl Copy for Score {}

impl PartialEq(Score) for Score {
    fn Eq(lhs: Score, rhs: Score) -> bool {
        lhs.val == rhs.val
    }
}

impl PartialOrd(Score) for Score {
    fn Lt(lhs: Score, rhs: Score) -> bool {
        lhs.val < rhs.val
    }
    fn Gt(lhs: Score, rhs: Score) -> bool {
        lhs.val > rhs.val
    }
}

fn main() -> i32 {
    let a = make Score { val: 5 };
    let b = make Score { val: 10 };
    let c = make Score { val: 5 };
    let result = 0;
    if a < b  { result = result + 1; };
    if b > a  { result = result + 2; };
    if a <= b { result = result + 4; };
    if a <= c { result = result + 8; };
    if b >= a { result = result + 16; };
    if c >= a { result = result + 32; };
    if a == c { result = result + 64; };
    if a != b { result = result + 128; };
    result
}
