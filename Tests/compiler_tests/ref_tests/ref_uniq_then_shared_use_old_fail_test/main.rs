fn main() -> i32 {
    let a = 5;
    let r1 = &mut a;
    let r2 = &a;
    *r1 + *r2
}
