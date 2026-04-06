use Std.Ops.PartialEq;

struct Point { x: i32, y: i32 }
impl Copy for Point {}

impl PartialEq(Point) for Point {
    fn Eq(lhs: Point, rhs: Point) -> bool {
        lhs.x == rhs.x && lhs.y == rhs.y
    }
}

fn main() -> i32 {
    let a = make Point { x: 1, y: 2 };
    let b = make Point { x: 1, y: 2 };
    let c = make Point { x: 3, y: 4 };
    let r1 = if a == b { 1 } else { 0 };
    let r2 = if a == c { 1 } else { 0 };
    r1 + r2
}
