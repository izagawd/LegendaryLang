fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    let r1: &i32 = &*b;
    let r2: &i32 = &*b;
    *r1 + *r2 - 42
}
