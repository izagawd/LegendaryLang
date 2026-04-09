fn main() -> i32 {
    let a = 42;
    let b = &a;
    let c = &a;
    *b + *c
}
