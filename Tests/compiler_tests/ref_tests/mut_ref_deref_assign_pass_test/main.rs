fn main() -> i32 {
    let x = 0;
    let r = &mut x;
    *r = 42;
    x
}
