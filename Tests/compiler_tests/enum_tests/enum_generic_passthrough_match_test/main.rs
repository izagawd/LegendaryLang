fn Pass[T:! Sized](x: T) -> T { x }

fn main() -> i32 {
    let x = Pass(Option.Some(42));
    match x {
        Option.Some(v) => v,
        Option.None => 0
    }
}
