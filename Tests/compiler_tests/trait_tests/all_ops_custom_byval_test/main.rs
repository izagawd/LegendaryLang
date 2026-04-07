use Std.Ops.PartialEq;
use Std.Ops.PartialOrd;

struct Val { n: i32 }
impl Copy for Val {}

impl PartialEq(Val) for Val {
    fn Eq(lhs: Val, rhs: Val) -> bool { lhs.n == rhs.n }
}

impl PartialOrd(Val) for Val {
    fn Lt(lhs: Val, rhs: Val) -> bool { lhs.n < rhs.n }
    fn Gt(lhs: Val, rhs: Val) -> bool { lhs.n > rhs.n }
}

fn main() -> i32 {
    let a = make Val { n: 3 };
    let b = make Val { n: 7 };
    let c = make Val { n: 3 };
    let result = 0;
    if a == c { result = result + 1; };
    if a != b { result = result + 2; };
    if a < b  { result = result + 4; };
    if b > a  { result = result + 8; };
    if a <= c { result = result + 16; };
    if a <= b { result = result + 32; };
    if b >= a { result = result + 64; };
    if c >= a { result = result + 128; };
    result
}
