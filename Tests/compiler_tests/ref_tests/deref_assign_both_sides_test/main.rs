fn main() -> i32 {
    let x = 42;
    let y = 0;
    let rx = &x;
    let ry = &mut y;
    *ry = *rx;
    y
}
