struct Holder['a]{
    r: &'a mut i32
}

fn Pass1[T:! Sized](input: T) -> T { input }
fn Pass2[T:! Sized](input: T) -> T { input }

fn main() -> i32 {
    let x = 5;
    let h = make Holder { r: &mut x };
    let h2 = Pass2(Pass1(h));
    x = 10;
    return x;
}
