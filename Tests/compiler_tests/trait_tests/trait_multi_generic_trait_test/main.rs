trait Combiner(A:! Sized, B:! Sized): Sized {
    fn combine(a: A, b: B) -> Self;
}

impl Combiner(i32, i32) for i32 {
    fn combine(a: i32, b: i32) -> i32 {
        a + b
    }
}

fn main() -> i32 {
    (i32 as Combiner(i32, i32)).combine(10, 20)
}
