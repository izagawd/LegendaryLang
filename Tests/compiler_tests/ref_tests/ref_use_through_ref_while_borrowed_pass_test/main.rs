struct Point {
    x: i32,
    y: i32
}
fn main() -> i32 {
    let p = make Point { x: 3, y: 7 };
    let r = &uniq p;
    r.x + r.y
}
