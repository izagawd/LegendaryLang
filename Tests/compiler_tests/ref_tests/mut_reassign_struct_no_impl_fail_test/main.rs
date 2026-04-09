struct Foo { val: i32 }

fn main() -> i32 {
    let f = make Foo { val: 0 };
    let r = &mut f;
    *r = make Foo { val: 99 };
    f.val
}
