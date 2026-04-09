fn main() -> i32 {
    let a = 5;
    let b = &a;
    let a = 10;
    let b = &a;
    *b
}
