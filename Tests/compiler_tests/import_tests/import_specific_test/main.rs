use Std.Ops.Add;

struct Vec2 {
    x: i32,
    y: i32
}

impl Copy for Vec2 {}

impl Add(Vec2) for Vec2 {
    let Output :! type = i32;
    fn Add(lhs: Vec2, rhs: Vec2) -> i32 {
        lhs.x + rhs.x + lhs.y + rhs.y
    }
}

fn main() -> i32 {
    let a = make Vec2 { x: 10, y: 11 };
    let b = make Vec2 { x: 10, y: 11 };
    a + b
}
