fn main() -> i32 {
    let b: Gc(i32) = Gc.New(0);
    let w: &mut i32 = &mut *b;
    *w = 42;
    let r: &i32 = &*b;
    *r
}
