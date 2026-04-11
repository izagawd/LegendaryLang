fn main() -> i32 {
    let a = 5;
    let r1 = &a;
    let r2 = &mut a;
    *r1
}
