fn main() -> i32 {
    let x = 5;
    let r = &x;
    *r = 10;
    x
}
