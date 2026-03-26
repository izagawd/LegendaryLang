struct Vec2 {
    x: i32,
    y: i32
}

impl Copy for Vec2 {}

impl Add<Vec2> for Vec2 {
    type Output = Vec2;
    fn add(lhs: Vec2, rhs: Vec2) -> Vec2 {
        Vec2 { x = lhs.x + rhs.x, y = lhs.y + rhs.y }
    }
}

fn main() -> i32 {
    let a = Vec2 { x = 1, y = 2 };
    let b = Vec2 { x = 3, y = 4 };
    let c = a + b;
    c.x + c.y
}
