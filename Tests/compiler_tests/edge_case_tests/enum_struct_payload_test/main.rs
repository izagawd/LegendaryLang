struct Point {
    x: i32,
    y: i32
}
enum Shape {
    Circle(i32),
    Dot(Point)
}
fn main() -> i32 {
    let p = Point { x = 3, y = 4 };
    let s = Shape::Dot(p);
    match s {
        Shape::Circle(r) => r,
        Shape::Dot(pt) => pt.x + pt.y
    }
}
