use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let result = TryCastPrimitive(usize, 0 - 5);
    match result {
        Option.Some(val) => 0 - 1,
        Option.None => 1
    }
}
