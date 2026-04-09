use Std.Ops.PartialEq;

struct Pair { x: i32, y: i32 }
impl Copy for Pair {}

impl PartialEq(Pair) for Pair {
    fn Eq(lhs: &Pair, rhs: &Pair) -> bool {
        lhs.x == rhs.x && lhs.y == rhs.y
    }
}

fn main() -> i32 {
    let a = make Pair { x: 1, y: 2 };
    let b = make Pair { x: 1, y: 2 };
    let eq1 = a == b;
    let eq2 = a == b;
    let eq3 = a == b;
    let result = 0;
    if eq1 { result = result + 1; };
    if eq2 { result = result + 10; };
    if eq3 { result = result + 100; };
    result
}
