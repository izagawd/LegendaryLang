struct Foo { val: i32 }
fn TakeOwnership[T:! Sized](input: T) {}
fn use_ref[T:! Sized](x: &T) -> i32 { 0 }
fn main() -> i32 {
    let a = make Foo { val: 5 };
    use_ref(&a);
    TakeOwnership(a);
    5
}
