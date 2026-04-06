fn main() -> i32 {
    let a = 10;
    let r = &mut a;
    *r = *r + 5;
    a
}
