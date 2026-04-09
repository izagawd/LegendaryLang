struct Point { x: i32, y: i32 }
impl Copy for Point {}

enum Shape {
    Circle(i32),
    Rect(i32, i32)
}

fn center(s: Shape) -> Point {
    match s {
        Shape.Circle(r) => make Point { x: r, y: r },
        Shape.Rect(w, h) => make Point { x: w, y: h }
    }
}

fn main() -> i32 {
    let p = center(Shape.Rect(3, 7));
    p.x + p.y
}
