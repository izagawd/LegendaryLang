struct Foo { kk: i32 }
fn TakeOwnership[T:! type](input: T) {}

fn main() -> i32 {
    let a = make Foo { kk: 5 };
    let r: &Foo = &a;
    let val = r.kk;
    TakeOwnership(a);
    val
}
