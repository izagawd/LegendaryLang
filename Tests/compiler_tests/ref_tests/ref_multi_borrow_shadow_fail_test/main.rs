fn main() -> i32 {
    let a = 5;
    let b = &a;
    let c = &a;
    let a = 10;
    *b + *c
}
