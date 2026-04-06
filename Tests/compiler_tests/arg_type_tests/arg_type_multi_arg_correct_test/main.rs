struct Point { x: i32, y: i32 }
impl Copy for Point {}
fn sum(p: Point, scale: i32) -> i32 {
    (p.x + p.y) * scale
}
fn main() -> i32 {
    let p = make Point { x: 10, y: 11 };
    sum(p, 2)
}
