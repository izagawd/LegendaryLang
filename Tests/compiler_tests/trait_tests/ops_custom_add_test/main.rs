struct Vec2 {
    x: i32,
    y: i32
}

impl Copy for Vec2 {}

impl Add<Vec2> for Vec2 {
    type Output = i32;
    fn add(lhs: Vec2, rhs: Vec2) -> i32 {
        lhs.x + rhs.x + lhs.y + rhs.y
    }
}

fn main() -> i32 {
    let a = Vec2 { x = 1, y = 2 };
    let b = Vec2 { x = 3, y = 4 };
    <Vec2 as Add<Vec2>>::add(a, b)
}
