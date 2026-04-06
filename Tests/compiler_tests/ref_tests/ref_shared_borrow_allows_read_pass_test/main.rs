fn main() -> i32 {
    let x = 42;
    let r = &x;
    x + *r
}
