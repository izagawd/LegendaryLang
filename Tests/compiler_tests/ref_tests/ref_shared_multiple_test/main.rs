fn main() -> i32 {
    let a = 5;
    let r1 = &a;
    let r2 = &a;
    let r3 = &a;
    *r1 + *r2 + *r3
}
