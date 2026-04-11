struct Foo { val: i32 }

fn mut_write(f: &mut Foo) {
    f.val = 42;
}

fn main() -> i32 {
    let f = make Foo { val: 0 };
    mut_write(&mut f);
    f.val
}
