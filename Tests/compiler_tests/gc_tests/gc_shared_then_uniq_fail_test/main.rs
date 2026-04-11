fn main() -> i32 {
    let b: Gc(i32) = Gc.New(42);
    let r1: &i32 = &*b;
    let r2: &mut i32 = &mut *b;
    *r1 + *r2
}
