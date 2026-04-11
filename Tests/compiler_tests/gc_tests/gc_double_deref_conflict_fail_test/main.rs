fn main() -> i32 {
    let b: Gc(i32) = Gc.New(42);
    let r: &mut i32 = &mut *b;
    let s: &i32 = &*b;
    *r + *s
}
