fn main() -> i32 {
    let a = 0;
    let r = &mut a;
    *r = 42;
    a
}
