struct Point {
    x: i32,
    y: i32
}

impl Copy for Point {}

fn main() -> i32 {
    let a = Point { x = 3, y = 4 };
    let b = a;
    a.x + b.y
}
