fn main() -> i32 {
    let a = 5;
    let r1 = &a;
    let r2 = &r1;
    **r2
}
