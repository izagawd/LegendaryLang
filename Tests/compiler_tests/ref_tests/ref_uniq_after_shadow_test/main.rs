fn main() -> i32 {
    let a = 5;
    let r1 = &a;
    let a = 10;
    let r2 = &mut a;
    *r2
}
