struct Point {
    x: i32,
    y: i32
}
impl Copy for Point {}
fn main() -> i32 {
    let p = make Point { x : 3, y : 4 };
    let r = &p;
    r.x + r.y
}
