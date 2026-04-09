struct Foo { kk: i32 }
fn TakeOwnership[T:! type](input: T) {}

fn take_ref[T:! type](r: &T) -> i32 { 0 }

fn main() -> i32 {
    let a = make Foo { kk: 5 };
    let r = &a;
    take_ref(r);
    TakeOwnership(a);
    5
}
