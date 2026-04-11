fn main() -> i32 {
    let x = 10;
    let r = &mut x;
    let y = x;
    *r + y
}
