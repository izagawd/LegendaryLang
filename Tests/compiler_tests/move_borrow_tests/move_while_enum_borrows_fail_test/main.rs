struct Foo { kk: i32 }
enum MaybeRef['a] { Some(&'a Foo), None }
fn TakeOwnership[T:! Sized](input: T) {}

fn main() -> i32 {
    let a = make Foo { kk: 5 };
    let m = MaybeRef.Some(&a);
    TakeOwnership(a);
    5
}
