fn main() -> i32 {
    let a = 5;
    let b = &a;
    let c = *b;
    let a = 10;
    c
}
