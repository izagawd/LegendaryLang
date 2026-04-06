struct Vec2 {
    x: i32,
    y: i32
}

impl Copy for Vec2 {}

impl Std.Ops.Add(Vec2) for Vec2 {
    type Output = i32;
    fn Add(lhs: Vec2, rhs: Vec2) -> i32 {
        lhs.x + rhs.x + lhs.y + rhs.y
    }
}

fn main() -> i32 {
    let a = make Vec2 { x : 1, y : 2 };
    let b = make Vec2 { x : 3, y : 4 };
    (Vec2 as Std.Ops.Add(Vec2)).Add(a, b)
}
