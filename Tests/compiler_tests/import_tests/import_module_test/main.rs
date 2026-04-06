use Std.Ops;

struct Pair {
    x: i32,
    y: i32
}

impl Copy for Pair {}

impl Ops.Add(Pair) for Pair {
    type Output = i32;
    fn Add(lhs: Pair, rhs: Pair) -> i32 {
        lhs.x + rhs.x + lhs.y + rhs.y
    }
}

fn main() -> i32 {
    let a = make Pair { x: 10, y: 11 };
    let b = make Pair { x: 10, y: 11 };
    a + b
}
