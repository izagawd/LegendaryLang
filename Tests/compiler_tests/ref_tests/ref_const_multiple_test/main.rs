fn main() -> i32 {
    let a = 5;
    let r1 = &const a;
    let r2 = &const a;
    *r1 + *r2
}
