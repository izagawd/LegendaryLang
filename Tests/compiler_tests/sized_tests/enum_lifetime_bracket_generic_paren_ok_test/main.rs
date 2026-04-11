enum MaybeRef['a](T:! Sized) {
    Some(&'a T),
    None
}
fn main() -> i32 {
    let x: i32 = 5;
    let m = MaybeRef.Some(&x);
    match m {
        MaybeRef.Some(r) => *r,
        MaybeRef.None => 0
    }
}
