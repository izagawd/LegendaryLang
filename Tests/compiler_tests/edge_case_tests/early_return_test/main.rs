fn check(x: i32) -> i32 {
    if x > 10 {
        return 99;
    }
    x
}
fn main() -> i32 {
    check(5) + check(20)
}
