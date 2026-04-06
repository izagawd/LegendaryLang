fn main() -> i32 {
    let x = 10;
    let r = &x;
    let val = *r;
    x + val
}
