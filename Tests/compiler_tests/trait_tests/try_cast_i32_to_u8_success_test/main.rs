use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let result = TryCastPrimitive(u8, 42);
    match result {
        Option.Some(val) => 1,
        Option.None => 0
    }
}
